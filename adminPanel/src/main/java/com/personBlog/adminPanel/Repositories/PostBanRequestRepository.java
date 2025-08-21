package com.personBlog.adminPanel.Repositories;

import com.personBlog.adminPanel.Entities.PostBanRequest;
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

    @Query("SELECT p FROM PostBanRequest p WHERE p.processed = false")
    Page<PostBanRequest> findAllByProcessedEqualsTrueOrderByCreatedAtAsc(Pageable pageable);

    @Query("SELECT p from PostBanRequest p where p.processed=false and p.postId= :postId")
    List<PostBanRequest> findAllByPostId(UUID postId);

}
