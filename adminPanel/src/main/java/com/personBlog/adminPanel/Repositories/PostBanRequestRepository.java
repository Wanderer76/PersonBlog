package com.personBlog.adminPanel.Repositories;

import com.personBlog.adminPanel.Entities.PostBanRequest;
import com.personBlog.adminPanel.Services.Models.PostToBanItem;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface PostBanRequestRepository extends JpaRepository<PostBanRequest, Long> {

    @Query("select count(p) from PostBanRequest p where p.processed = false")
    long countActiveRequests();

    @Query("select count(p.postId) from PostBanRequest p where p.processed = false group by p.postId")
    long countActiveRequestsByPost();

    @Query("SELECT p.postId, count(p) FROM PostBanRequest p WHERE p.processed = false group by p.postId")
    Page<PostToBanItem> findAllByProcessedEqualsTrueOrderByCreatedAtAsc(Pageable pageable);

    @Query("SELECT p FROM PostBanRequest p WHERE p.processed = false")
    Page<PostBanRequest> findAllByPostIdOrderByCreatedAtAsc(UUID postId, Pageable pageable);

    @Query("SELECT p from PostBanRequest p where p.processed=false and p.postId= :postId")
    List<PostBanRequest> findAllByPostId(UUID postId);

}
